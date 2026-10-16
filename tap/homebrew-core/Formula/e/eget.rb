class Eget < Formula
  desc "Easily install prebuilt binaries from GitHub"
  homepage "https://github.com/zyedidia/eget"
  url "https://github.com/zyedidia/eget/releases/download/v1.3.4/eget-1.3.4-windows_amd64.zip"
  sha256 "27ffb63d5e2e17a829653a4c65a9412fe45fae2be56478167e3b89aaa8ffe899"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/zyedidia/eget/archive/refs/tags/v1.3.4.tar.gz"
  end

  def install
    bin.install "eget.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/eget --version 2>&1")
  end
end
