class OrcTools < Formula
  desc "ORC java command-line tools and utilities"
  homepage "https://orc.apache.org/"
  url "https://search.maven.org/remotecontent?filepath=org/apache/orc/orc-tools/2.3.1/orc-tools-2.3.1-uber.jar"
  sha256 "05110c26350d384da0315291b13490a1aa4879d8b80af1d1117fd072d46bdff0"
  license "Apache-2.0"

  livecheck do
    url "https://search.maven.org/remotecontent?filepath=org/apache/orc/orc-tools/"
    regex(%r{href=["']?v?(\d+(?:\.\d+)+)/?["' >]}i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "f4608d25d1fb4f91a04ddad8f6ed52822b1fd9d7baacbcd33a560b7f405e81b5"
  end

  depends_on "openjdk"

  def install
    libexec.install "orc-tools-#{version}-uber.jar"
    bin.write_jar_script libexec/"orc-tools-#{version}-uber.jar", "orc-tools"
  end

  test do
    system bin/"orc-tools", "meta", "-h"
  end
end
