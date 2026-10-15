class Richgo < Formula
  desc "Enrich `go test` outputs with text decorations"
  homepage "https://github.com/kyoh86/richgo"
  url "https://github.com/kyoh86/richgo/releases/download/v0.3.12/richgo_0.3.12_windows_amd64.zip"
  sha256 "cdea6c88aae107614acbf63ceb3f9a5d2cc650cf1bce710eba1ebdeccdfda059"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/kyoh86/richgo/archive/refs/tags/v0.3.12.tar.gz"
  end

  def install
    bin.install "richgo.exe"
  end

  test do
    assert_path_exists bin/"richgo.exe"
  end
end
