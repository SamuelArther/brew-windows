class Cheops < Formula
  desc "CHEss OPponent Simulator"
  homepage "https://logological.org/cheops"
  url "https://files.nothingisreal.com/software/cheops/cheops-1.3.tar.bz2"
  mirror "https://github.com/logological/cheops/releases/download/1.3/cheops-1.3.tar.bz2"
  sha256 "a3ce2e94f73068159827a1ec93703b5075c7edfdf5b0c1aba4d71b3e43fe984e"
  license "GPL-3.0-or-later"

  livecheck do
    url "https://files.nothingisreal.com/software/cheops/"
    regex(/href=.*?cheops[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/cheops-1.3"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f64ffafe5170adcc8e55c56ae9b485f24d7687a1fd9344186baf647648db2a92"
  end

  deny_network_access!

  def install
    system "./configure", "--disable-dependency-tracking",
                          "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    system bin/"cheops", "--version"
  end
end
