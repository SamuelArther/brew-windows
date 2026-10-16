class OpenjdkAT21 < Formula
  desc "Development kit for the Java programming language"
  homepage "https://openjdk.org/"
  url "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.12.1%2B1/OpenJDK21U-jdk_x64_windows_hotspot_21.0.12.1_1.zip"
  version "21.0.12.1_1"
  sha256 "f9d6e191ab098c0d416e7d588a24420a8621cd2f4720dab2459b8b7b2d2d8b4e"
  license "GPL-2.0-only" => { with: "Classpath-exception-2.0" }

  livecheck do
    url "https://api.adoptium.net/v3/assets/latest/21/hotspot?architecture=x64&image_type=jdk&os=windows&vendor=eclipse"
    regex(/jdk-(\d+(?:\.\d+)*\+\d+)/i)
  end

  # Eclipse Temurin JDK 21 (an official, TCK-tested OpenJDK build from the Adoptium project).
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
    assert_match "21", shell_output("#{bin}/java -version 2>&1")
  end
end
