class Tailor < Formula
  desc "Cross-platform static analyzer and linter for Swift"
  homepage "https://sleekbyte.github.io/tailor/"
  url "https://github.com/sleekbyte/tailor/releases/download/v0.12.0/tailor-0.12.0.tar"
  sha256 "ec3810b27e9a35ecdf3a21987f17cad86918240d773172264e9abbb1a7efc415"
  license "MIT"
  revision 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "b206fa704b15e94c54b553b513afe2374979debeefbaf3c82b8c448109bf72dd"
  end

  depends_on "openjdk"

  def install
    libexec.install Dir["*"]
    (bin/"tailor").write_env_script libexec/"bin/tailor", JAVA_HOME: formula_opt_prefix("openjdk")
    man1.install libexec/"tailor.1"
  end

  test do
    (testpath/"Test.swift").write "import Foundation\n"
    system bin/"tailor", testpath/"Test.swift"
  end
end
