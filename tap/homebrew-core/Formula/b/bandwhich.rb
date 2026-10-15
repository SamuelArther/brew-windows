class Bandwhich < Formula
  desc "Terminal bandwidth utilization tool"
  homepage "https://github.com/imsnif/bandwhich"
  url "https://github.com/imsnif/bandwhich/releases/download/v0.23.1/bandwhich-v0.23.1-x86_64-pc-windows-msvc.zip"
  sha256 "684d87acfd0fa808700500c2d4ced2a60be222dd86adc671035ad7b84c42cda4"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/imsnif/bandwhich/archive/refs/tags/v0.23.1.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/bandwhich.exe", "bandwhich"
  end

  test do
    assert_path_exists bin/"bandwhich.exe"
  end
end
