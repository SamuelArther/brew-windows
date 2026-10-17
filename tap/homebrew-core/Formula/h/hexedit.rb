class Hexedit < Formula
  desc "View and edit files in hexadecimal or ASCII"
  homepage "https://rigaux.org/hexedit.html"
  url "https://github.com/pixel/hexedit/archive/refs/tags/1.6.tar.gz"
  sha256 "598906131934f88003a6a937fab10542686ce5f661134bc336053e978c4baae3"
  license "GPL-2.0-or-later"
  head "https://github.com/pixel/hexedit.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/hexedit-1.6"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f0234021209e1cf80f3e5b69f94f61f14be50b41ef15400a3a5d0ab0aec5ecd8"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build

  uses_from_macos "ncurses"

  def install
    system "./autogen.sh"
    system "./configure", "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    shell_output("#{bin}/hexedit -h 2>&1", 1)
  end
end
