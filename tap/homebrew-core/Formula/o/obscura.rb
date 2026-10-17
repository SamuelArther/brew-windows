class Obscura < Formula
  desc "Headless browser for AI agents and web scraping"
  homepage "https://obscura.sh"
  url "https://github.com/h4ckf0r0day/obscura/archive/refs/tags/v0.2.4.tar.gz"
  sha256 "e8cfbad9025bd79d4f22da55e2c0f9111b8a082b821805f258c6be2594f25111"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8ded4766da0af6ee1d8f2fb5497a5af91f7e8f50452fcb7731c42c6a91a9e0c3"
  end

  depends_on "rust" => :build

  def install
    system "cargo", "install", *std_cargo_args(path: "crates/obscura-cli")
  end

  test do
    output = shell_output(
      "#{bin}/obscura fetch 'data:text/html,<title>Homebrew Test</title>' --eval 'document.title'",
    )
    assert_equal "Homebrew Test\n", output

    # obscura blocks fetches to loopback/private addresses by default (SSRF protection)
    blocked = shell_output("#{bin}/obscura fetch http://127.0.0.1:1/ 2>&1", 1)
    assert_match "private/internal IP address", blocked
  end
end
