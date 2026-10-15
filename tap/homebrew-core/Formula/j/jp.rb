class Jp < Formula
  desc "Dead simple terminal plots from JSON data"
  homepage "https://github.com/sgreben/jp"
  url "https://github.com/sgreben/jp/releases/download/1.1.12/jp_1.1.12_windows_x86_64.zip"
  sha256 "d943cf0f5a0e079364544021d0fc2f639bb36b578cd0182ab93ff486e9345bb1"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sgreben/jp/archive/refs/tags/1.1.12.tar.gz"
  end

  def install
    bin.install "jp.exe"
  end

  test do
    assert_path_exists bin/"jp.exe"
  end
end
