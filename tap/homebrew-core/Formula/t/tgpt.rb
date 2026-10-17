class Tgpt < Formula
  desc "AI Chatbots in terminal without needing API keys"
  homepage "https://github.com/aandrew-me/tgpt"
  url "https://github.com/aandrew-me/tgpt/archive/refs/tags/v2.15.0.tar.gz"
  sha256 "208f8d96ef1b1af85c765d3dde45842fd7df18936fb4e121d52538d0da1be032"
  license "GPL-3.0-only"
  head "https://github.com/aandrew-me/tgpt.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "aef952ebe62bf2e71f43642cb4234014d559386547f31b4bd6bd2c1c745a16eb"
  end

  depends_on "go" => :build

  on_linux do
    depends_on "libx11"
  end

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tgpt --version")

    # The free default providers keep changing their access rules, so query a local port with nothing listening
    url = "http://127.0.0.1:#{free_port}/v1/chat/completions"
    output = shell_output("#{bin}/tgpt --quiet --provider ollama --url #{url} 'What is 1+1' 2>&1", 1)
    # Windows words a refused connection "No connection could be made because the target machine actively refused it".
    assert_match OS.windows? ? "actively refused it" : "connect: connection refused", output
  end
end
