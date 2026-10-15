class Openjdk < Formula
  desc "Development kit for the Java programming language"
  homepage "https://openjdk.org/"
  version "27"
  url "https://github.com/adoptium/temurin27-binaries/releases/download/jdk-27%2B35/OpenJDK27U-jdk_x64_windows_hotspot_27_35.zip"
  sha256 "2031c9c5c624737ed29a0624caaf5bcc25e74235834c28b4bfa88f7ba7a40e4a"
  license "GPL-2.0-only" => { with: "Classpath-exception-2.0" }

  livecheck do
    url "https://api.adoptium.net/v3/assets/latest/27/hotspot?architecture=x64&image_type=jdk&os=windows&vendor=eclipse"
    regex(/jdk-(\d+(?:\.\d+)*\+\d+)/i)
  end

  # Eclipse Temurin JDK 27 (an official, TCK-tested OpenJDK build from the Adoptium project),
  # used because openjdk.org does not publish Windows GA zips for every release.

  def install
    install_tree
    %w[
      jar jarsigner java javac javadoc javap javaw jcmd jconsole jdb jdeprscan jdeps jfr jhsdb
      jimage jinfo jlink jmap jmod jnativescan jpackage jps jshell jstack jstat jstatd jwebserver
      keytool kinit klist ktab rmiregistry serialver
    ].each do |cmd|
      windows_exe "libexec/bin/#{cmd}.exe", cmd
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
  end
end
