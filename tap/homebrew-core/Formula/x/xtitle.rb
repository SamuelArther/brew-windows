class Xtitle < Formula
  desc "Set window title and icon for your X terminal"
  homepage "https://kinzler.com/me/xtitle/"
  url "https://kinzler.com/me/xtitle/xtitle-1.0.4.tgz"
  sha256 "cadddef1389ba1c5e1dc7dd861545a5fe11cb397a3f692cd63881671340fcc15"
  license "GPL-2.0-or-later"

  livecheck do
    url :homepage
    regex(/href=.*?xtitle[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "da06410dfb24cdbae3d59e92f6808ba825863f89cec86492d5700d7f1da7ea46"
  end

  def install
    bin.install "xtitle.sh" => "xtitle"
    bin.install "xtctl.sh" => "xtctl"
    man1.install "xtitle.man" => "xtitle.1"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/xtitle --version")
  end
end
