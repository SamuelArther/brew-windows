class DvdVr < Formula
  desc "Utility to identify and extract recordings from DVD-VR files"
  homepage "https://www.pixelbeat.org/programs/dvd-vr/"
  url "https://www.pixelbeat.org/programs/dvd-vr/dvd-vr-0.9.7.tar.gz"
  sha256 "19d085669aa59409e8862571c29e5635b6b6d3badf8a05886a3e0336546c938f"
  license "GPL-2.0-or-later"

  livecheck do
    url :homepage
    regex(/href=.*?dvd-vr[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/dvd-vr-0.9.7"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "00400b157ed7ab84d44cda765dfe162708ff1dc2b4551e81c9c395d6e327430a"
  end

  def install
    system "make", "PREFIX=#{prefix}", "install"
  end

  test do
    system bin/"dvd-vr", "--version"
  end
end
