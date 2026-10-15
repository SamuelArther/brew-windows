class Groovy < Formula
  desc "Java-based scripting language"
  homepage "https://www.groovy-lang.org/"
  url "https://www.apache.org/dyn/closer.lua?path=groovy/6.0.0/distribution/apache-groovy-binary-6.0.0.zip"
  mirror "https://archive.apache.org/dist/groovy/6.0.0/distribution/apache-groovy-binary-6.0.0.zip"
  sha256 "54ab1a877f01da2ed16ed43c1ed7a8d6c36b122714c6959712700e89e6efde7c"
  license "Apache-2.0"

  livecheck do
    url "https://groovy.apache.org/download.html"
    regex(/href=.*?apache-groovy-binary[._-]v?(\d+(?:\.\d+)+)\.zip/i)
  end

  depends_on "openjdk"

  # Official Apache Groovy binary zip; its .bat launchers use JAVA_HOME or the java on PATH (openjdk).

  def install
    install_tree
    %w[grape groovy groovyc groovyConsole groovydoc groovysh java2groovy].each do |cmd|
      windows_cmd "libexec/bin/#{cmd}.bat", cmd
    end
  end

  test do
    output = shell_output("#{bin}/grape install org.activiti activiti-engine 5.16.4 2>&1")
    assert_match "found org.activiti#activiti-engine;5.16.4", output
    assert_match "65536\n===> null\n", pipe_output("#{bin}/groovysh", "println 64*1024\n:exit\n")
  end
end
