class Vnu < Formula
  desc "Nu Markup Checker: command-line and server HTML validator"
  homepage "https://validator.github.io/validator/"
  url "https://registry.npmjs.org/vnu-jar/-/vnu-jar-26.11.20.tgz"
  sha256 "ebd60284d4c918754d4e21204db6ac46fb59108313ea69d6cbf93e009e616e45"
  license "MIT"
  version_scheme 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "0fcea3bd5a02338b4554805433badb479ca34476ea29c269a31022055cb6d8fa"
  end

  depends_on "openjdk"

  def install
    libexec.install "build/dist/vnu.jar"
    bin.write_jar_script libexec/"vnu.jar", "vnu"
  end

  test do
    (testpath/"index.html").write <<~HTML
      <!DOCTYPE html>
      <html>
      <head>
        <title>hi</title>
      </head>
      <body>
      </body>
      </html>
    HTML
    system bin/"vnu", testpath/"index.html"
  end
end
