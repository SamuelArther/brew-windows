class Ivy < Formula
  desc "Agile dependency manager"
  homepage "https://ant.apache.org/ivy/"
  url "https://www.apache.org/dyn/closer.lua?path=ant/ivy/2.6.0/apache-ivy-2.6.0-bin.tar.gz"
  mirror "https://archive.apache.org/dist/ant/ivy/2.6.0/apache-ivy-2.6.0-bin.tar.gz"
  sha256 "3ada1eaadfaddf5347f1de8303d283dc535f4d8bddd7bee7b32e397c8327cb1f"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "58b323a12f6432c9b39bd1bfa5fae41c32ea6ca7d077f8df7634ce5e31599f24"
  end

  depends_on "openjdk"

  deny_network_access!

  def install
    libexec.install Dir["ivy*"]
    doc.install Dir["doc/*"]
    bin.write_jar_script libexec/"ivy-#{version}.jar", "ivy", "$JAVA_OPTS"
  end

  test do
    (testpath/"ivy.xml").write <<~XML
      <ivy-module version="2.0">
        <info organisation="org.example" module="test"/>
      </ivy-module>
    XML

    output = shell_output("#{bin}/ivy -ivy ivy.xml -retrieve 'lib/[artifact]-[revision].[ext]'")
    assert_match "0 artifacts copied", output
  end
end
