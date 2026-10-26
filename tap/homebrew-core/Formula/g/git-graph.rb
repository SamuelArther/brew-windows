class GitGraph < Formula
  desc "Command-line tool to show clear git graphs arranged for your branching model"
  homepage "https://github.com/git-bahn/git-graph"
  url "https://github.com/git-bahn/git-graph/archive/refs/tags/v0.8.1.tar.gz"
  sha256 "0694bcf963c7a0ba865303fa03f1b7382031271e651625ac55a97aeb340f36b4"
  license "MIT"
  head "https://github.com/git-bahn/git-graph.git", branch: "master"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "2f73f48ce3201e0a7c8e927a61a1332d2ba1fea0ebd8b7158ad5bc04169a95a0"
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
