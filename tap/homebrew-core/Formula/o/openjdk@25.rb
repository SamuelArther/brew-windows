class OpenjdkAT25 < Formula
  desc "Development kit for the Java programming language"
  homepage "https://openjdk.org/"
  url "https://github.com/adoptium/temurin25-binaries/releases/download/jdk-25.0.4.1%2B1/OpenJDK25U-jdk_x64_windows_hotspot_25.0.4.1_1.zip"
  version "25.0.4.1_1"
  sha256 "00c847d804f4a78e9f04f2683faf14fed898535b177b7fc704486cb0284e9283"
  license "GPL-2.0-only" => { with: "Classpath-exception-2.0" }

  livecheck do
    url "https://api.adoptium.net/v3/assets/latest/25/hotspot?architecture=x64&image_type=jdk&os=windows&vendor=eclipse"
    regex(/jdk-(\d+(?:\.\d+)*\+\d+)/i)
  end

  # Eclipse Temurin JDK 25 (an official, TCK-tested OpenJDK build from the Adoptium project).
  keg_only :versioned_formula

  def install
    install_tree
    Dir[libexec/"bin/*.exe"].each do |exe|
      windows_exe "libexec/bin/#{File.basename(exe)}", File.basename(exe, ".exe")
    end
  end

  test do
    (testpath/"HelloWorld.java").write <<~JAVA
      class HelloWorld {
        public static void main(String args[]) {
          System.out.println("Hello, world!");
        }
      }
    JAVA

    system bin/"javac", "HelloWorld.java"
    assert_match "Hello, world!", shell_output("#{bin}/java HelloWorld")
    assert_match "25", shell_output("#{bin}/java -version 2>&1")
  end
end
