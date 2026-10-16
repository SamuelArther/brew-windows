class Tygo < Formula
  desc "Generate Typescript types from Golang source code"
  homepage "https://github.com/gzuidhof/tygo"
  url "https://github.com/gzuidhof/tygo/releases/download/v0.2.21/tygo_v0.2.21_windows_amd64.zip"
  sha256 "a4754ab84112a7a9fa9e634122ecb8ea29f7ad4b083660500107d019a311bd74"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/gzuidhof/tygo.git"
  end

  def install
    bin.install "tygo.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tygo --version 2>&1")
  end
end
