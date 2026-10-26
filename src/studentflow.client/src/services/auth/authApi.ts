import type { AuthUser, LoginRequest, RegisterRequest } from '../../models/auth/AuthTypes'

/// <summary>
/// Chyba přihlášení kvůli nesprávným údajům.
/// </summary>
export class InvalidCredentialsError extends Error {
  constructor() {
    super('Nesprávný e-mail nebo heslo.')
    this.name = 'InvalidCredentialsError'
  }
}

/// <summary>
/// Účet v dočasném úložišti. Heslo se uchovává jen kvůli mocku.
/// </summary>
interface MockAccount {
  user: AuthUser
  password: string
}

/// <summary>
/// Testovací účet pro vývoj. Je to student, který může zakládat projekty.
/// TODO: odstranit spolu s mockem, až bude hotové API a seed v databázi.
/// </summary>
export const devTestAccount = {
  email: 'kulisek@email.com',
  password: 'kulisek123',
} as const

/// <summary>
/// Dočasné úložiště účtů. Nově zaregistrované účty zůstanou jen do obnovení stránky.
/// </summary>
const mockAccounts: MockAccount[] = [
  {
    user: { id: 1, name: 'Kulíšek', email: devTestAccount.email, accountType: 'student' },
    password: devTestAccount.password,
  },
]

/// <summary>
/// Porovná e-maily bez ohledu na velikost písmen.
/// </summary>
function sameEmail(first: string, second: string): boolean {
  return first.trim().toLowerCase() === second.trim().toLowerCase()
}

/// <summary>
/// Počká na dočasnou odpověď a respektuje zrušení požadavku.
/// </summary>
async function wait(signal?: AbortSignal) {
  await new Promise((resolve) => window.setTimeout(resolve, 300))
  signal?.throwIfAborted()
}

/// <summary>
/// Přihlásí uživatele.
/// TODO: nahradit voláním POST /api/auth/login (401 = InvalidCredentialsError).
/// </summary>
export async function login(request: LoginRequest, signal?: AbortSignal): Promise<AuthUser> {
  await wait(signal)

  const account = mockAccounts.find((item) => sameEmail(item.user.email, request.email))
  if (!account || account.password !== request.password) throw new InvalidCredentialsError()

  return account.user
}

/// <summary>
/// Zaregistruje nový účet.
/// TODO: nahradit voláním POST /api/auth/register.
/// </summary>
export async function register(request: RegisterRequest, signal?: AbortSignal): Promise<void> {
  await wait(signal)

  if (mockAccounts.some((item) => sameEmail(item.user.email, request.email))) {
    throw new Error('Účet s tímto e-mailem už existuje.')
  }

  mockAccounts.push({
    user: {
      id: mockAccounts.length + 1,
      name: request.name,
      email: request.email,
      accountType: request.accountType,
    },
    password: request.password,
  })
}
