class Vnu < Formula
  desc "Nu Markup Checker: command-line and server HTML validator"
  homepage "https://validator.github.io/validator/"
  url "https://registry.npmjs.org/vnu-jar/-/vnu-jar-26.11.17.tgz"
  sha256 "1ad40463b3f9498d263cf350497b6bd9a9fc6278cb8d1183539bf4fd22b4a247"
  license "MIT"
  version_scheme 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "70153534fe8801fcf36652f303536d7934c4037d4a92f673965f9bc3aa27d776"
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
