class HicolorIconTheme < Formula
  desc "Fallback theme for FreeDesktop.org icon themes"
  homepage "https://wiki.freedesktop.org/www/Software/icon-theme/"
  url "https://icon-theme.freedesktop.org/releases/hicolor-icon-theme-0.18.tar.xz"
  sha256 "db0e50a80aa3bf64bb45cbca5cf9f75efd9348cf2ac690b907435238c3cf81d7"
  license "GPL-2.0-only"
  head "https://gitlab.freedesktop.org/xdg/default-icon-theme.git", branch: "master"

  # The homepage hasn't been updated to link to more recent versions, so we
  # have to check the directory listing page instead.
  livecheck do
    url "https://icon-theme.freedesktop.org/releases/"
    regex(/href=.*?hicolor-icon-theme[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/hicolor-icon-theme-0.18"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "dbe88653d370502bb4a15008fe1db5a827e506222fe2b3eb091c14010c4b1d3e"
  end

  depends_on "meson" => :build
  depends_on "ninja" => :build

  def install
    system "meson", "setup", "build", *std_meson_args
    system "meson", "compile", "-C", "build", "--verbose"
    system "meson", "install", "-C", "build"
  end

  test do
    assert_path_exists share/"icons/hicolor/index.theme"
  end
end
