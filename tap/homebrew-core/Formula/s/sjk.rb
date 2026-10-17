class Sjk < Formula
  desc "Swiss Java Knife"
  homepage "https://github.com/aragozin/jvm-tools"
  url "https://search.maven.org/remotecontent?filepath=org/gridkit/jvmtool/sjk-plus/0.23/sjk-plus-0.23.jar"
  sha256 "6aab07cdf0ecad394e225a1f47d7342cb23bfd8b7d5c65c945f81835363ec937"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "dfbbd79ead361afdbd3e58c7b466d81bb0d9b684d3aa31974c76a1ab7a1952e9"
  end

  depends_on "openjdk"

  def install
    libexec.install "sjk-plus-#{version}.jar"
    bin.write_jar_script libexec/"sjk-plus-#{version}.jar", "sjk"
  end

  test do
    system bin/"sjk", "jps"
  end
end
