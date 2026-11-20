class Vnu < Formula
  desc "Nu Markup Checker: command-line and server HTML validator"
  homepage "https://validator.github.io/validator/"
  url "https://registry.npmjs.org/vnu-jar/-/vnu-jar-26.11.19.tgz"
  sha256 "61747e95cc861d445319eca4afdb2dac4688e5386a1f694f8a654fe8971d7823"
  license "MIT"
  version_scheme 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "08d8709245ee5736438eb2c0f769a1c4a84709c186b6c6e2c31ccda548084e07"
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
