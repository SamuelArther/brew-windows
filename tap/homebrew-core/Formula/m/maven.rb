class Maven < Formula
  desc "Java-based project management"
  homepage "https://maven.apache.org/"
  version "3.10.0"
  url "https://archive.apache.org/dist/maven/maven-3/3.10.0/binaries/apache-maven-3.10.0-bin.zip"
  sha256 "1f6d9909266510f039f59aa0e13dcd2c66da85f043e56276e41f2918f8bddaff"
  license "Apache-2.0"

  resource "jdk" do
    url "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.12.1%2B1/OpenJDK21U-jdk_x64_windows_hotspot_21.0.12.1_1.zip"
    sha256 "f9d6e191ab098c0d416e7d588a24420a8621cd2f4720dab2459b8b7b2d2d8b4e"
  end

  # Includes a pinned Eclipse Temurin JDK 21 resource; JAVA_HOME is scoped to Maven wrappers.

  def install
    install_tree
    install_resource "jdk", "libexec/jdk"
    windows_cmd "libexec/bin/mvn.cmd", "mvn", "JAVA_HOME", "libexec/jdk"
    windows_cmd "libexec/bin/mvnDebug.cmd", "mvnDebug", "JAVA_HOME", "libexec/jdk"
  end
end
