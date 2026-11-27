class CratesTui < Formula
  desc "TUI for exploring crates.io using Ratatui"
  homepage "https://github.com/ratatui/crates-tui"
  url "https://github.com/ratatui/crates-tui/archive/refs/tags/v0.1.26.tar.gz"
  sha256 "ea2a844f1e9a2ece95c3a09ad165c18529847b48141cf1a52a18eb9191ea8f45"
  license "MIT"
  head "https://github.com/ratatui/crates-tui.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "5572891de7c37c9a92499c8b71c9c5df0730cd271b4379e16cb30871814aeed1"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  on_linux do
    depends_on "openssl@4"
  end

  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    # vergen's old libgit2-sys calls advapi32 without linking it, which Rust's std no longer does for it.
    ENV.append "RUSTFLAGS", "-l advapi32" if OS.windows?
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/crates-tui --version")

    begin
      output_log = testpath/"output.log"
      if OS.mac?
        pid = spawn bin/"crates-tui", [:out, :err] => output_log.to_s
      else
        require "pty"
        r, _w, pid = PTY.spawn(bin/"crates-tui", [:out, :err] => output_log.to_s)
        r.winsize = [80, 43]
      end
      sleep 2
      assert_match "New Crates", output_log.read
    ensure
      Process.kill("TERM", pid)
      Process.wait(pid)
    end
  end
end
