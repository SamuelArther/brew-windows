class GradleProfiler < Formula
  desc "Profiling and benchmarking tool for Gradle builds"
  homepage "https://github.com/gradle/gradle-profiler/"
  # TODO: Check if we can use `openjdk` 25+ when bumping the version.
  url "https://github.com/gradle/gradle-profiler/releases/download/v0.26.0/gradle-profiler-0.26.0.zip"
  sha256 "c573cdaffac8ecfa60cdd0624274511f46cbdde77fbba39ce35eae0fae7c9877"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "414c8697a62a6bc541050ffd3febc6489a5e95d74067068b75067ef6d5769333"
  end

  depends_on "openjdk@21"

  def install
    rm(Dir["bin/*.bat"])
    libexec.install %w[bin lib]
    env = Language::Java.overridable_java_home_env("21")
    (bin/"gradle-profiler").write_env_script libexec/"bin/gradle-profiler", env
  end

  test do
    (testpath/"settings.gradle").write ""
    (testpath/"build.gradle").write 'println "Hello"'
    output = shell_output("#{bin}/gradle-profiler --gradle-version 8.14 --profile chrome-trace")
    assert_includes output, "* Writing results to"
  end
end
