class Smartypants < Formula
  desc "Typography prettifier"
  homepage "https://daringfireball.net/projects/smartypants/"
  url "https://daringfireball.net/projects/downloads/SmartyPants_1.5.1.zip"
  sha256 "2813a12d8dd23f091399195edd7965e130103e439e2a14f298b75b253616d531"
  license "BSD-3-Clause"

  livecheck do
    url :homepage
    regex(/href=.*?SmartyPants[._-]v?(\d+(?:\.\d+)+)\.zip/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "5adbc6325fe26f914d2b7a13aafcc5afc8183bb250af413f83b1c431e820bec9"
  end

  def install
    bin.install "SmartyPants.pl" => "smartypants"
  end

  test do
    assert_equal "&#8220;Give me a beer&#8221;, said Mike O&#8217;Connor",
      pipe_output(bin/"smartypants",
                  %q("Give me a beer", said Mike O'Connor), 0)
  end
end
