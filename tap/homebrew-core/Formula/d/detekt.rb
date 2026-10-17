class Detekt < Formula
  desc "Static code analysis for Kotlin"
  homepage "https://detekt.dev"
  url "https://github.com/detekt/detekt/releases/download/v1.23.8/detekt-cli-1.23.8-all.jar"
  sha256 "2ce2ff952e150baf28a29cda70a363b0340b3e81a55f43e51ec5edffc3d066c1"
  license "Apache-2.0"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "f38e77e3e9e40e3157c7ababb8b34c5a9141da46a19d7650bd30f422bec43b19"
  end

  depends_on "openjdk@21"

  def install
    libexec.install "detekt-cli-#{version}-all.jar"
    bin.write_jar_script libexec/"detekt-cli-#{version}-all.jar", "detekt", java_version: "21"
  end

  test do
    # generate default config for testing
    system bin/"detekt", "--generate-config"
    assert_match "empty-blocks:", File.read(testpath/"detekt.yml")

    (testpath/"input.kt").write <<~KOTLIN
      fun main() {

      }
    KOTLIN

    shell_output("#{bin}/detekt --input input.kt --report txt:output.txt --config #{testpath}/detekt.yml", 2)
    assert_equal "EmptyFunctionBlock", shell_output("cat output.txt").slice(/\w+/)
  end
end
