class Spark < Formula
  desc "Sparklines for the shell"
  homepage "https://zachholman.com/spark/"
  url "https://github.com/holman/spark/archive/refs/tags/v1.0.1.tar.gz"
  sha256 "a81c1bc538ce8e011f62264fe6f33d28042ff431b510a6359040dc77403ebab6"
  license "MIT"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/spark-1.0.1"
    sha256 cellar: :any, x86_64_windows: "052cb8deb1a147bf4773bf11c3e0efa0a24daa12fd0bc601b2cfd8c9d1379ebf"
  end

  def install
    bin.install "spark"
  end

  test do
    system bin/"spark"
  end
end
