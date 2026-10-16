class Kubecolor < Formula
  desc "Colorize your kubectl output"
  homepage "https://kubecolor.github.io/"
  url "https://github.com/kubecolor/kubecolor/releases/download/v0.8.0/kubecolor_0.8.0_windows_amd64.zip"
  sha256 "64e1b12fc56662bf4bb67a760a396b0d7306c30774a52714e26c9ac075cef20b"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/kubecolor/kubecolor/archive/refs/tags/v0.8.0.tar.gz"
  end

  def install
    bin.install "kubecolor.exe"
  end

  test do
    assert_path_exists bin/"kubecolor.exe"
  end
end
