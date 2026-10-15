class Kotlin < Formula
  desc "Statically typed programming language for the JVM"
  homepage "https://kotlinlang.org/"
  version "2.4.21"
  url "https://github.com/JetBrains/kotlin/releases/download/v2.4.21/kotlin-compiler-2.4.21.zip"
  sha256 "7cc140e76daf416a0424a5557f99afd7ca89ebd463ea1101261e3e01c33e75cd"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  depends_on "openjdk"

  # Official Kotlin compiler zip; its .bat launchers use JAVA_HOME or the java on PATH (openjdk).

  def install
    install_tree
    %w[kapt kotlin kotlinc kotlinc-js kotlinc-jvm].each do |cmd|
      windows_cmd "libexec/bin/#{cmd}.bat", cmd
    end
  end

  test do
    (testpath/"test.kt").write <<~KOTLIN
      fun main(args: Array<String>) {
        println("Hello World!")
      }
    KOTLIN
    system bin/"kotlinc", "test.kt", "-include-runtime", "-d", "test.jar"
    system bin/"kotlinc-js", "test.kt", "-Xir-produce-js", "-ir-output-dir", "out"
  end
end
