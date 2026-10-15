class Meson < Formula
  desc "Fast and user friendly build system"
  homepage "https://mesonbuild.com/"
  url "https://github.com/mesonbuild/meson/releases/download/1.12.1/meson-1.12.1-64.msi"
  sha256 "0e541b6ae656cd9c8f9d5f8ac9eddc22c68d37006ae1dfd055e3ab485519804d"
  license "Apache-2.0"

  # Official Meson Windows MSI, read as an archive (no installer runs).

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/**/meson.exe", "meson"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/meson --version")
  end
end
