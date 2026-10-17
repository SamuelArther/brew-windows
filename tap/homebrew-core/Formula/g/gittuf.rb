class Gittuf < Formula
  desc "Security layer for Git repositories"
  homepage "https://gittuf.dev/"
  url "https://github.com/gittuf/gittuf/releases/download/v0.16.0/gittuf.src.tar.gz"
  sha256 "9044c6a11fc810ab91157e8811610297518486d5dc0e7725f1695e98b60cd5b4"
  license "Apache-2.0"
  head "https://github.com/gittuf/gittuf.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "34226aef8452cec95c31e1a831246b52112118dd56e9340b8392fa22b5ccf5a5"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/gittuf/gittuf/internal/version.gitVersion=#{version}"
    system "go", "build", *std_go_args(ldflags:)
    system "go", "build", *std_go_args(ldflags:, output: bin/"git-remote-gittuf"), "./internal/git-remote-gittuf"

    generate_completions_from_executable(bin/"gittuf", shell_parameter_format: :cobra)
  end

  test do
    system "git", "init"

    output = shell_output("#{bin}/gittuf policy init 2>&1", 1)
    assert_match(
      /Error: (required flag "signing-key" not set|signing key not specified in Git configuration)/,
      output,
    )

    output = shell_output("#{bin}/gittuf sync 2>&1", 1)
    assert_match "Error:", output
    assert_match(/(unable to identify git directory for repository|No such remote 'origin')/, output)

    output = shell_output("#{bin}/git-remote-gittuf 2>&1", 1)
    # Windows prints the program's own path, with backslashes and its .exe.
    usage = OS.windows? ? "#{bin}/git-remote-gittuf.exe".tr("/", "\\") : "#{bin}/git-remote-gittuf"
    assert_match "usage: #{usage} <remote-name> <url>", output

    assert_match version.to_s, shell_output("#{bin}/gittuf version")
  end
end
