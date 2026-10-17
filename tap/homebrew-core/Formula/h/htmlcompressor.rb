class Htmlcompressor < Formula
  desc "Minify HTML or XML"
  homepage "https://code.google.com/archive/p/htmlcompressor/"
  url "https://storage.googleapis.com/google-code-archive-downloads/v2/code.google.com/htmlcompressor/htmlcompressor-1.5.3.jar"
  sha256 "88894e330cdb0e418e805136d424f4c262236b1aa3683e51037cdb66310cb0f9"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "4d562b2d310acc46cf996c56c4b489e1396488c395f82257815a3eb855feecef"
  end

  depends_on "openjdk"

  def install
    libexec.install "htmlcompressor-#{version}.jar"
    bin.write_jar_script libexec/"htmlcompressor-#{version}.jar", "htmlcompressor"
  end

  test do
    path = testpath/"index.xml"
    path.write <<~HTML
      <foo>
        <bar /> <!-- -->
      </foo>
    HTML

    output = shell_output("#{bin}/htmlcompressor #{path}").strip
    assert_equal "<foo><bar/></foo>", output
  end
end
