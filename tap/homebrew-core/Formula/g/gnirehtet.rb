class Gnirehtet < Formula
  desc "Reverse tethering tool for Android"
  homepage "https://github.com/Genymobile/gnirehtet"
  url "https://github.com/Genymobile/gnirehtet/releases/download/v2.5.1/gnirehtet-rust-win64-v2.5.1.zip"
  sha256 "7f5b1063e7895182aa60def1437e50363c3758144088dcd079037bb7c3c46a1c"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Genymobile/gnirehtet/archive/refs/tags/v2.5.1.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/gnirehtet.exe", "gnirehtet"
  end

  test do
    assert_path_exists bin/"gnirehtet.exe"
  end
end
