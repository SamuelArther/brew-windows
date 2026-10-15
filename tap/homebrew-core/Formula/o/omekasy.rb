class Omekasy < Formula
  desc "Converts alphanumeric input to various Unicode styles"
  homepage "https://github.com/ikanago/omekasy"
  url "https://github.com/ikanago/omekasy/releases/download/v1.3.3/omekasy-v1.3.3-x86_64-pc-windows-msvc.zip"
  sha256 "0609868a91db655f6b446c807890bc40d200b3176258eca148b45851ba5a084f"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ikanago/omekasy/archive/refs/tags/v1.3.3.tar.gz"
  end

  def install
    bin.install "omekasy.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/omekasy --version")
    output = shell_output("#{bin}/omekasy -f monospace Hello")
    assert_match "𝙷𝚎𝚕𝚕𝚘", output
  end
end
