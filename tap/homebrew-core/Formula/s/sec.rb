class Sec < Formula
  desc "Event correlation tool for event processing of various kinds"
  homepage "https://simple-evcorr.sourceforge.net/"
  url "https://github.com/simple-evcorr/sec/releases/download/2.9.4/sec-2.9.4.tar.gz"
  sha256 "77fd945980d15ca07f94a9cad6484677f5d3fe8ded5da12ec2c0c444ae7b0994"
  license "GPL-2.0-or-later"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/sec-2.9.4"
    sha256 cellar: :any, x86_64_windows: "5d5f67d0bc6e85efe3b4a47e04b79614ab2599eeb7ce7c9179f2ef9237bc45d3"
  end

  def install
    bin.install "sec"
    man1.install "sec.man" => "sec.1"
  end

  test do
    system bin/"sec", "--version"
  end
end
