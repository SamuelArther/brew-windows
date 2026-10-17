class Pacapt < Formula
  desc "Package manager in the style of Arch's pacman"
  homepage "https://github.com/icy/pacapt"
  url "https://github.com/icy/pacapt/archive/refs/tags/v3.0.7.tar.gz"
  sha256 "d1081b639466de7650ed66c7bb5a522482c60c24b03c292c46b86a3983e66234"
  license "Fair"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/pacapt-3.0.7"
    sha256 cellar: :any, x86_64_windows: "6ce9a8d35b873ae2f54779444bc39915d28c4014403ba27e6445b57dd3f54e72"
  end

  def install
    bin.mkpath
    system "make", "install", "BINDIR=#{bin}", "VERSION=#{version}"
  end

  test do
    system bin/"pacapt", "-Ss", "wget"
  end
end
