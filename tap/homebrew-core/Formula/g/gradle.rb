class Gradle < Formula
  desc "Open-source build automation tool based on the Groovy and Kotlin DSL"
  homepage "https://www.gradle.org/"
  url "https://services.gradle.org/distributions/gradle-9.8.1-bin.zip"
  sha256 "dce76f55f8e251a3a1f130eb120f30b3d271de2b76c9b0729d316b5a1b6dc01f"
  license "Apache-2.0"

  livecheck do
    url "https://gradle.org/install/"
    regex(/href=.*?gradle[._-]v?(\d+(?:\.\d+)+)-all\.(?:zip|t)/i)
  end

  depends_on "openjdk"

  # Official Gradle binary distribution; gradle.bat uses JAVA_HOME or the java on PATH (openjdk).

  def install
    install_tree
    windows_cmd "libexec/bin/gradle.bat", "gradle"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gradle --version")

    (testpath/"settings.gradle").write ""
    (testpath/"build.gradle").write <<~GRADLE
      println gradle.gradleVersion
    GRADLE
    assert_match version.to_s, shell_output("#{bin}/gradle build --no-daemon")
  end
end
