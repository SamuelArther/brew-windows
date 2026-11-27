class Vnu < Formula
  desc "Nu Markup Checker: command-line and server HTML validator"
  homepage "https://validator.github.io/validator/"
  url "https://registry.npmjs.org/vnu-jar/-/vnu-jar-26.11.26.tgz"
  sha256 "7b2bc855b7128232a733191fa06de5df7e01496b39668746f82c02b6f4ec94bf"
  license "MIT"
  version_scheme 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "8a5607d92fe0dbb1f47fee2a7779b9669563f140496c85db6bb77f85b84c0955"
  end

  depends_on "openjdk"

  deny_network_access!

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
