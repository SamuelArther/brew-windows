class SpringLoaded < Formula
  desc "Java agent to enable class reloading in a running JVM"
  homepage "https://github.com/spring-projects/spring-loaded"
  url "https://search.maven.org/remotecontent?filepath=org/springframework/springloaded/1.2.6.RELEASE/springloaded-1.2.6.RELEASE.jar"
  version "1.2.6"
  sha256 "6edd6ffb3fd82c3eee95f5588465f1ab3a94fc5fff65b6e3a262f6de5323d203"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "83c64ffb820de06c058379d3e370292a1d13a3bbd97003385d11bee6a42efc3a"
  end

  depends_on "openjdk" => :test

  def install
    (share/"java").install "springloaded-#{version}.RELEASE.jar" => "springloaded.jar"
  end

  test do
    system formula_opt_bin("openjdk")/"java", "-javaagent:#{share}/java/springloaded.jar", "-version"
  end
end
