class Openfasttrace < Formula
  desc "Requirement tracing suite"
  homepage "https://github.com/itsallcode/openfasttrace"
  url "https://github.com/itsallcode/openfasttrace/releases/download/4.12.1/openfasttrace-4.12.1.jar"
  sha256 "c2fada67389e8af202d60c78bf007c8134d3b96a735705d05f52b56993eec529"
  license "GPL-3.0-only"

  bottle do
    sha256 cellar: :any, x86_64_windows: "6af62257989b0ec2c7dd16cf519ce987385852153ab940ffc4b62ba9a4a581fb"
  end

  depends_on "openjdk"

  deny_network_access!

  def install
    libexec.install "openfasttrace-#{version}.jar"
    bin.write_jar_script libexec/"openfasttrace-#{version}.jar", "oft"
  end

  test do
    (testpath/"trace.md").write <<~MARKDOWN
      # Features
      `feat~tracing~1`

      Needs: req

      # Requirements
      `req~sample.requirement~1`

      Covers:
      * `feat~tracing~1`

      Needs: dsn

      # Design
      `dsn~sample.design~1`

      Covers:
      * `req~sample.requirement~1`
    MARKDOWN

    assert_equal "ok - 3 total", shell_output("#{bin}/oft trace --color-scheme BLACK_AND_WHITE #{testpath}").strip
  end
end
