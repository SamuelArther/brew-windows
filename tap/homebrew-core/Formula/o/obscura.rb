class Obscura < Formula
  desc "Headless browser for AI agents and web scraping"
  homepage "https://obscura.sh"
  url "https://github.com/h4ckf0r0day/obscura/archive/refs/tags/v0.2.5.tar.gz"
  sha256 "af287fa7658270a68ad6f1220197eb55035614a91b14295117da78e55494958a"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "3372d402c3de78aa8554ed136f2fc80bb2ba2727b9bc55834f17a6b3210b10ea"
  end

  depends_on "rust" => :build

  def install
    system "cargo", "install", *std_cargo_args(path: "crates/obscura-cli", features: "render")
  end

  test do
    output = shell_output(
      "#{bin}/obscura fetch 'data:text/html,<title>Homebrew Test</title>' --eval 'document.title'",
    )
    assert_equal "Homebrew Test\n", output

    assert_equal "rgb(255, 0, 0)\n", shell_output(
      %Q(#{bin}/obscura fetch 'data:text/html,<style>p{color:red}</style><p>x' \
      --eval 'getComputedStyle(document.querySelector("p")).color'),
    )

    # obscura blocks fetches to loopback/private addresses by default (SSRF protection)
    blocked = shell_output("#{bin}/obscura fetch http://127.0.0.1:1/ 2>&1", 1)
    assert_match "private/internal IP address", blocked
  end
end
