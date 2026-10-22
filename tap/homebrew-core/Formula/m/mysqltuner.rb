class Mysqltuner < Formula
  desc "Increase performance and stability of a MySQL installation"
  homepage "https://mysqltuner.com/"
  url "https://github.com/jmrenouard/MySQLTuner-perl/archive/refs/tags/v2.9.3.tar.gz"
  sha256 "19580893f2cb3be94bc5a73329935a3c045a15d209077b72f34213628fbdd18b"
  license "GPL-3.0-or-later"
  head "https://github.com/jmrenouard/MySQLTuner-perl.git", branch: "master"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "455788126cc95fcb7e818f5a739b75730c04bbe09e71f06a45214701ec38b6ba"
  end

  def install
    bin.install "mysqltuner.pl" => "mysqltuner"
  end

  # mysqltuner analyzes your database configuration by connecting to a
  # mysql server. It is not really feasible to spawn a mysql server
  # just for a test case so we'll stick with a rudimentary test.
  test do
    system bin/"mysqltuner", "--help"
  end
end
