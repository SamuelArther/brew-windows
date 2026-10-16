class OpenjdkAT17 < Formula
  desc "Development kit for the Java programming language"
  homepage "https://openjdk.org/"
  url "https://github.com/adoptium/temurin17-binaries/releases/download/jdk-17.0.20.1%2B1/OpenJDK17U-jdk_x64_windows_hotspot_17.0.20.1_1.zip"
  version "17.0.20.1_1"
  sha256 "e53a79c3c3d86865bd7e787903884331068e71321714ffd44f145785affc7cb0"
  license "GPL-2.0-only" => { with: "Classpath-exception-2.0" }

  livecheck do
    url "https://api.adoptium.net/v3/assets/latest/17/hotspot?architecture=x64&image_type=jdk&os=windows&vendor=eclipse"
    regex(/jdk-(\d+(?:\.\d+)*\+\d+)/i)
  end

  # Eclipse Temurin JDK 17 (an official, TCK-tested OpenJDK build from the Adoptium project).
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
    assert_match "17", shell_output("#{bin}/java -version 2>&1")
  end
end
