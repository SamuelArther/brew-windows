class Turso < Formula
  desc "Interactive SQL shell for Turso"
  homepage "https://github.com/tursodatabase/turso"
  url "https://github.com/tursodatabase/turso/archive/refs/tags/v0.8.4.tar.gz"
  sha256 "c678c98a1f7a7ed1cf609bfbc5655198de4c6e19d62a941875d4d161584aecde"
  license "MIT"
  head "https://github.com/tursodatabase/turso.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "207b02d6fcdd0f40befc389b5d5c44e68f9d5a0e32bd88df4009e616bb4faacb"
  end

  depends_on "rust" => :build
  uses_from_macos "sqlite" => :test

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    # Rust 1.100 resolves std `Vec::new_in` first: https://github.com/tursodatabase/turso/issues/9463
    inreplace "core/index_method/fts/directory.rs", "DynVec::new_in(",
              "<DynVec<u8> as TursoVecInExt<u8, DynAllocator>>::new_in("

    system "cargo", "install", *std_cargo_args(path: "cli")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tursodb --version")

    data = %w[Bob 14 Sue 12 Tim 13]
    create = "create table students (name text, age integer);\n"
    data.each_slice(2) do |n, a|
      create << "insert into students (name, age) values ('#{n}', '#{a}');\n"
    end
    pipe_output("sqlite3 school.sqlite", create, 0)

    begin
      output_log = testpath/"output.log"
      if OS.mac?
        pid = spawn bin/"tursodb", "school.sqlite", [:out, :err] => output_log.to_s
      else
        require "pty"
        r, _w, pid = PTY.spawn bin/"tursodb", "school.sqlite", [:out, :err] => output_log.to_s
        r.winsize = [80, 43]
      end
      sleep 2
      assert_match "\".help\" for usage hints.", output_log.read
    ensure
      Process.kill("TERM", pid)
      Process.wait(pid)
    end
  end
end
