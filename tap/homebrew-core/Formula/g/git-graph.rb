class GitGraph < Formula
  desc "Command-line tool to show clear git graphs arranged for your branching model"
  homepage "https://github.com/git-bahn/git-graph"
  url "https://github.com/git-bahn/git-graph/archive/refs/tags/v0.8.0.tar.gz"
  sha256 "906ef8f5931c32324ab372ac0047b6440faf76b8ce61fda811d8b85cdc3da577"
  license "MIT"
  head "https://github.com/git-bahn/git-graph.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "ced369dd71ba882c1a92bfa8d47a92b58edb39871a6d3a23c14e3db81d5110f6"
  end

  depends_on "rust" => :build

  on_linux do
    depends_on "zlib-ng-compat"
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/git-graph --version")

    system "git", "init"
    system "git", "commit", "--allow-empty", "-m", "Initial commit"

    begin
      output_log = testpath/"output.log"
      pid = spawn bin/"git-graph", [:out, :err] => output_log.to_s
      sleep 1
      assert_match "Initial commit", output_log.read
    ensure
      Process.kill("TERM", pid)
      Process.wait(pid)
    end
  end
end
