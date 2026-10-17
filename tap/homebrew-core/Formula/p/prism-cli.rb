class PrismCli < Formula
  desc "Set of packages for API mocking and contract testing"
  homepage "https://stoplight.io/open-source/prism"
  url "https://registry.npmjs.org/@stoplight/prism-cli/-/prism-cli-5.16.0.tgz"
  sha256 "666ebea18de24d25766c6b0c6f8eaacb0f2fd1e695635d53dd78638c7fe25b89"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "860386eb7788d57cada28c956d60d4c27ed47e7832fc82b9b9f6b75a2bcf54c8"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args(ignore_scripts: false)
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    port = free_port
    pid = spawn bin/"prism", "mock", "--port", port.to_s, "https://raw.githubusercontent.com/OAI/OpenAPI-Specification/refs/tags/3.1.1/examples/v3.0/petstore.yaml"

    sleep 10

    system "curl", "http://127.0.0.1:#{port}/pets"

    assert_match version.to_s, shell_output("#{bin}/prism --version")
  ensure
    Process.kill("TERM", pid)
    Process.wait(pid)
  end
end
