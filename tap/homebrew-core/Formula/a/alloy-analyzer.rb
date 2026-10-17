class AlloyAnalyzer < Formula
  desc "Open-source language and analyzer for software modeling"
  homepage "https://alloytools.org"
  url "https://search.maven.org/remotecontent?filepath=org/alloytools/org.alloytools.alloy.dist/6.2.0/org.alloytools.alloy.dist-6.2.0.jar"
  sha256 "6037cbeee0e8423c1c468447ed10f5fcf2f2743a2ffc39cb1c81f2905c0fdb9d"
  license "Apache-2.0"

  livecheck do
    url "https://search.maven.org/remotecontent?filepath=org/alloytools/org.alloytools.alloy.dist/maven-metadata.xml"
    regex(%r{<version>(\d+(?:\.\d+)+)</version>}i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "c678bd5be52f4f946654379bf8944e2e72534a6bb969bd6a618961147503ac99"
  end

  depends_on "openjdk"

  conflicts_with "grafana-alloy", because: "both install `alloy` binaries"

  deny_network_access!

  def install
    libexec.install "org.alloytools.alloy.dist-#{version}.jar"
    bin.write_jar_script libexec/"org.alloytools.alloy.dist-#{version}.jar", "alloy"
  end

  test do
    output = shell_output("#{bin}/alloy version 2>&1")
    filtered_output = output.lines.reject { |line| line.start_with?("Picked up") }.join
    assert_match version.to_s, filtered_output
  end
end
