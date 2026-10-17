class Jhiccup < Formula
  desc "Measure pauses and stalls of an app's Java runtime platform"
  homepage "https://www.azul.com/products/components/jhiccup/"
  url "https://www.azul.com/wp-content/uploads/jHiccup-2.0.10-dist.zip"
  sha256 "7bb1145d211d140b4f81184df7eb9cea90f56720ad7504fac43c0c398f38a7d8"
  license any_of: ["CC0-1.0", "BSD-2-Clause"]

  livecheck do
    url :homepage
    regex(/href=.*?jHiccup[._-]v?(\d+(?:\.\d+)+)-dist(?:-\d+)?\.zip/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "33045d403e94cbaa0e67a4f424aeb3b28073ab1d573fac0e086a98dbba0fb1d2"
  end

  def install
    bin.install "jHiccup", "jHiccupLogProcessor"

    # Simple script to create and open a new plotter spreadsheet
    (bin/"jHiccupPlotter").write <<~SH
      #!/bin/sh
      TMPFILE="/tmp/jHiccupPlotter.$$.xls"
      cp "#{prefix}/jHiccupPlotter.xls" $TMPFILE
      open $TMPFILE
    SH

    prefix.install "jHiccup.jar"
    prefix.install "jHiccupPlotter.xls"
    inreplace "#{bin}/jHiccup" do |s|
      s.gsub!(/^JHICCUP_JAR_FILE=.*$/,
              "JHICCUP_JAR_FILE=#{prefix}/jHiccup.jar")
    end
  end

  test do
    assert_match "CSV", shell_output("#{bin}/jHiccup -h", 255)
  end
end
