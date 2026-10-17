class Igv < Formula
  desc "Interactive Genomics Viewer"
  homepage "https://igv.org/doc/desktop/"
  url "https://data.broadinstitute.org/igv/projects/downloads/2.19/IGV_2.19.8.zip"
  sha256 "7476fc44f15788f52dfe1d40e99748c7aedcb9dbf6a91f7634a81344e50d07a6"
  license "MIT"

  livecheck do
    url "https://igv.org/doc/desktop/DownloadPage/"
    regex(/href=.*?IGV[._-]v?(\d+(?:\.\d+)+)\.zip/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "21af611cd979b76592ac0203293cc852e5d49d33b81185b86ae29120e86e0046"
  end

  depends_on "openjdk"

  def install
    inreplace ["igv.sh", "igvtools"], /^prefix=.*/, "prefix=#{libexec}"
    bin.install "igv.sh" => "igv"
    bin.install "igvtools"
    libexec.install "igv.args", "lib"
    bin.env_script_all_files libexec, Language::Java.overridable_java_home_env
  end

  test do
    assert_match "Usage:", shell_output("#{bin}/igvtools")
    assert_match "org/broad/igv/ui/IGV.class",
      shell_output("#{formula_opt_bin("openjdk")}/jar tf #{libexec}/lib/igv.jar")

    ENV.append "_JAVA_OPTIONS", "-Duser.home=#{testpath}"
    (testpath/"script").write "exit"
    assert_match "Using system JDK.", shell_output("#{bin}/igv -b script")
  end
end
