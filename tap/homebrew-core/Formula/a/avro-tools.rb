class AvroTools < Formula
  desc "Avro command-line tools and utilities"
  homepage "https://avro.apache.org/"
  url "https://www.apache.org/dyn/closer.lua?path=avro/avro-1.12.2/java/avro-tools-1.12.2.jar"
  mirror "https://archive.apache.org/dist/avro/avro-1.12.2/java/avro-tools-1.12.2.jar"
  sha256 "6220e8bc089aaf917cdad4cd358bd651fc0394c0e5ddb8b36da402012c294a68"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "ab9dc79b296da499f3b38877b98ab78ae811834850fd7f2cf59bf5132ad92dfd"
  end

  depends_on "openjdk"

  deny_network_access!

  def install
    libexec.install "avro-tools-#{version}.jar"
    bin.write_jar_script libexec/"avro-tools-#{version}.jar", "avro-tools"
  end

  test do
    assert_match "Version #{version}", shell_output("#{bin}/avro-tools 2>&1", 1)
  end
end
