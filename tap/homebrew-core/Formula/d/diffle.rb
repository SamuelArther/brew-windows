class Diffle < Formula
  desc "Keyboard-first diff viewer with LSP support and GitHub integration"
  homepage "https://github.com/moritzwilksch/diffle"
  url "https://github.com/moritzwilksch/diffle/archive/refs/tags/v0.3.5.tar.gz"
  sha256 "3d509db823aeda7a8524075e0bae73f789324c272159f91e61526f7a8d003b92"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a2bda784ee397ba8f610b91abe3db2f925950de1bd56a0c1bbeade3b8294497b"
  end

  depends_on "node" => :build
  depends_on "rust" => :build

  allow_network_access! :test

  def fetch
    system "npm", "ci", "--ignore-scripts", *std_npm_args(prefix: false)
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "npm", "run", "build"
    system "cargo", "install", *std_cargo_args(path: "crates/diffle")

    generate_completions_from_executable(bin/"diffle", "completion", "--shell")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/diffle --version")

    system "git", "init", "--initial-branch=main"
    system "git", "config", "user.name", "BrewTestBot"
    system "git", "config", "user.email", "BrewTestBot@test.com"
    (testpath/"hello.txt").write "hello\n"
    system "git", "add", "hello.txt"
    system "git", "commit", "-m", "initial"
    (testpath/"hello.txt").append_lines "world"

    port = free_port
    pid = spawn bin/"diffle", "working", "--port", port.to_s, "--no-open", "--no-lsp", "--no-watch", "--keep-alive"
    begin
      sleep 5
      assert_match "<html", shell_output("curl -s http://127.0.0.1:#{port}/")
      assert_match "+world", shell_output("curl -s http://127.0.0.1:#{port}/api/patch")
    ensure
      Process.kill "TERM", pid
      Process.wait pid
    end
  end
end
