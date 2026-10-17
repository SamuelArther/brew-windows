class When < Formula
  desc "Tiny personal calendar"
  homepage "https://www.lightandmatter.com/when/when.html"
  url "https://bitbucket.org/ben-crowell/when/get/1.1.45.tar.bz2"
  sha256 "fd614afe891b6e8e7b131041176e958fe00583c8300a3523ddfb7d65692a68df"
  license "GPL-2.0-only"
  head "https://bitbucket.org/ben-crowell/when.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "7cdc123dbd229150d3374a3db9ca6328db84fbc1ea94e32a4008203171a00475"
  end

  def install
    system "make", "prefix=#{prefix}", "install"
  end

  test do
    (testpath/".when/preferences").write <<~EOS
      calendar = #{testpath}/calendar
    EOS

    (testpath/"calendar").write "2015 April 1, stay off the internet"
    system bin/"when", "i"
  end
end
