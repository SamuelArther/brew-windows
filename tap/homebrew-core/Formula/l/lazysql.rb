class Lazysql < Formula
  desc "Cross-platform TUI database management tool"
  homepage "https://github.com/jorgerojas26/lazysql"
  url "https://github.com/jorgerojas26/lazysql/archive/refs/tags/v0.7.0.tar.gz"
  sha256 "735b5a987c8a84a699d34d0cba1a3bcce6d65a46cf608d96758df6257ca353f7"
  license "MIT"
  head "https://github.com/jorgerojas26/lazysql.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f775010e1bd6aeed908d132f3a4fb1d69bb10a179cb8485490559d9e47d52954"
  end

  depends_on "go" => :build
  uses_from_macos "sqlite" => :test

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ENV["CGO_ENABLED"] = OS.mac? ? "1" : "0"
    system "go", "build", *std_go_args(ldflags: "-X main.version=#{version}")
  end

  test do
    school = <<~SQL
      create table students (name text, age integer);
      insert into students (name, age) values ('Bob', 14);
      insert into students (name, age) values ('Sue', 12);
      insert into students (name, age) values ('Tim', 13);
      select name from students order by age asc;
    SQL

    names = pipe_output("sqlite3 test.db", school, 0).strip.split("\n")
    assert_equal %w[Sue Tim Bob], names

    # tcell drives the Windows console directly, not terminfo, so TERM=dumb does not stop the TUI there.
    assert_match "terminal not cursor addressable", shell_output("#{bin}/lazysql test.db 2>&1", 1) unless OS.windows?

    assert_match version.to_s, shell_output("#{bin}/lazysql -version 2>&1")
  end
end
