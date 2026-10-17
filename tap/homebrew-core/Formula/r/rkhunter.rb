class Rkhunter < Formula
  desc "Rootkit hunter"
  homepage "https://rkhunter.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/rkhunter/rkhunter/1.4.6/rkhunter-1.4.6.tar.gz"
  sha256 "f750aa3e22f839b637a073647510d7aa3adf7496e21f3c875b7a368c71d37487"
  license "GPL-2.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "468842d6a2b1de399db9f0bac9af0218b3c2db4a877843f04bc98c358d2a29bb"
  end

  def install
    system "./installer.sh", "--layout", "custom", prefix, "--install"
  end

  test do
    system bin/"rkhunter", "--version"
  end
end
