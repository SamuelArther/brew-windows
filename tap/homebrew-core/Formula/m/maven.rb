class Maven < Formula
  desc "Java-based project management"
  homepage "https://maven.apache.org/"
  url "https://www.apache.org/dyn/closer.lua?path=maven/maven-3/3.10.0/binaries/apache-maven-3.10.0-bin.zip"
  mirror "https://archive.apache.org/dist/maven/maven-3/3.10.0/binaries/apache-maven-3.10.0-bin.zip"
  sha256 "1f6d9909266510f039f59aa0e13dcd2c66da85f043e56276e41f2918f8bddaff"
  license "Apache-2.0"

  livecheck do
    url "https://maven.apache.org/download.cgi"
    regex(/href=.*?apache-maven[._-]v?(\d+(?:\.\d+)+)-bin\.t/i)
  end

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

  test do
    (testpath/"pom.xml").write <<~XML
      <?xml version="1.0" encoding="UTF-8"?>
      <project xmlns="https://maven.apache.org/POM/4.0.0" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
        xsi:schemaLocation="https://maven.apache.org/POM/4.0.0 http://maven.apache.org/maven-v4_0_0.xsd">
        <modelVersion>4.0.0</modelVersion>
        <groupId>org.homebrew</groupId>
        <artifactId>maven-test</artifactId>
        <version>1.0.0-SNAPSHOT</version>
        <properties>
          <maven.compiler.source>1.8</maven.compiler.source>
          <maven.compiler.target>1.8</maven.compiler.target>
          <project.build.sourceEncoding>UTF-8</project.build.sourceEncoding>
        </properties>
      </project>
    XML

    (testpath/"src/main/java/org/homebrew").mkpath
    (testpath/"src/main/java/org/homebrew/MavenTest.java").write <<~JAVA
      package org.homebrew;
      public class MavenTest {
        public static void main(String[] args) {
          System.out.println("Testing Maven with Homebrew!");
        }
      }
    JAVA

    system bin/"mvn", "compile", "-Duser.home=#{testpath}"
    assert_path_exists testpath/"target/classes/org/homebrew/MavenTest.class"
  end
end
