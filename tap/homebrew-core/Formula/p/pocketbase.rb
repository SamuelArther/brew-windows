class Pocketbase < Formula
  desc "Open source backend for your next project in 1 file"
  homepage "https://pocketbase.io/"
  url "https://github.com/pocketbase/pocketbase/releases/download/v0.40.5/pocketbase_0.40.5_windows_amd64.zip"
  sha256 "d0980d1f3a8097ff4c03a876e8c98f3008a83c0d42cc03b636bc92b2d4788d1c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/pocketbase/pocketbase/archive/refs/tags/v0.40.5.tar.gz"
  end

  def install
    bin.install "pocketbase.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pocketbase --version 2>&1")
  end
end
