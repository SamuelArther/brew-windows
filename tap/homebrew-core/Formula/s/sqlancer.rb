class Sqlancer < Formula
  desc "Detecting Logic Bugs in DBMS"
  homepage "https://github.com/sqlancer/sqlancer"
  url "https://github.com/sqlancer/sqlancer/archive/refs/tags/v2.0.0.tar.gz"
  sha256 "4811fea3d08d668cd2a41086be049bdcf74c46a6bb714eb73cdf6ed19a013f41"
  license "MIT"
  head "https://github.com/sqlancer/sqlancer.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "087f7160eb21d6f0976adecc1154f2df8e91f28e87bf52ef55be63b1f8a2488d"
  end

  depends_on "maven" => :build
  depends_on "openjdk"

  uses_from_macos "sqlite" => :test

  def install
    if build.head?
      inreplace "pom.xml", %r{<artifactId>sqlancer</artifactId>\n\s*<version>#{stable.version}</version>},
                             "<artifactId>sqlancer</artifactId>\n   <version>#{version}</version>"
    end
    system "mvn", "package", "-DskipTests=true",
                             "-Dmaven.javadoc.skip=true",
                             "-Djacoco.skip=true"
    libexec.install "target"
    bin.write_jar_script libexec/"target/sqlancer-#{version}.jar", "sqlancer"
  end

  test do
    cmd = %w[
      sqlancer
      --print-progress-summary true
      --num-threads 1
      --timeout-seconds 5
      --random-seed 1
      sqlite3
    ].join(" ")
    output = shell_output(cmd)

    assert_match(/Overall execution statistics/, output)
    assert_match(/\d+k? successfully-executed statements/, output)
    assert_match(/\d+k? unsuccessfuly-executed statements/, output)
  end
end
